using System;
using System.Collections.Generic;
using System.Data;
using developwithpassion.specifications;
using developwithpassion.specifications.core;
using developwithpassion.specifications.extensions;
using developwithpassion.specifications.faking;
using developwithpassion.specifications.rhinomocks;
using Machine.Fakes.Adapters.Rhinomocks;
using Machine.Fakes.Sdk;
using Machine.Specifications;

namespace developwithpassion.specification.specs
{
    public class SUTFactorySpecs
    {
        public abstract class concern : Observes
        {
            Establish base_setup = delegate
            {
                connection = fake.an<IDbConnection>();
                command = fake.an<IDbCommand>();
                manage_fakes = fake.an<IManageFakes>();
                constructor_parameters = new Dictionary<Type, object>();
                dependency_resolver = fake.an<IResolveADependencyForTheSUT>();
                dependency_resolver.setup(x => x.resolve(typeof(IDbConnection))).Return(connection);
                dependency_resolver.setup(x => x.resolve(typeof(IDbCommand))).Return(command);

                sut = new DefaultSUTFactory<ItemToCreate>(constructor_parameters,
                                                          dependency_resolver, manage_fakes);
            };

            protected static IDbCommand command;
            protected static IDbConnection connection;
            protected static IDictionary<Type, object> constructor_parameters;
            protected static IResolveADependencyForTheSUT dependency_resolver;
            protected static ICreateAndManageDependenciesFor<ItemToCreate> sut;
            protected static IManageFakes manage_fakes;
        }

        public abstract class concern_for_type_with_some_non_fakeable_ctor_parameters : Observes
        {
            Establish c = delegate
            {
                dependency_resolver = fake.an<IResolveADependencyForTheSUT>();
                manage_fakes = fake.an<IManageFakes>();
                original_exception = new Exception();
                the_connection = fake.an<IDbConnection>();
                the_command = fake.an<IDbCommand>();
                constructor_parameters = new Dictionary<Type, object>();
                dependency_resolver.setup(x => x.resolve(typeof(IDbConnection))).Return(the_connection);
                dependency_resolver.setup(x => x.resolve(typeof(IDbCommand))).Return(the_command);
                sut = new DefaultSUTFactory<ItemWithNonFakeableCtorParameters>(constructor_parameters,
                                                                               dependency_resolver,
                                                                               manage_fakes);
            };

            protected static IDictionary<Type, object> constructor_parameters;
            protected static IResolveADependencyForTheSUT dependency_resolver;
            protected static Exception original_exception;
            protected static DefaultSUTFactory<ItemWithNonFakeableCtorParameters> sut;
            protected static IDbCommand the_command;
            protected static IDbConnection the_connection;
            static IManageFakes manage_fakes;
        }

        public class when_requesting_a_dependency : concern_for_type_with_some_non_fakeable_ctor_parameters
        {
            
        }
        public class when_creating_a_type_that_has_constructor_parameters_that_cant_be_faked :
            concern_for_type_with_some_non_fakeable_ctor_parameters
        {
            static ItemWithNonFakeableCtorParameters result;

            public class and_arguments_have_not_been_specifically_provided :
                when_creating_a_type_that_has_constructor_parameters_that_cant_be_faked
            {
                Establish c = () =>
                    dependency_resolver.setup(x => x.resolve(typeof(SomeOtherType))).Throw(original_exception);

                Because b = () =>
                    spec.catch_exception(() => sut.create());

                It should_throw_the_original_exception_thrown_by_the_dependency_resolver = () =>
                    spec.exception_thrown.ShouldEqual(original_exception);
            }

            public class and_the_arguments_have_been_specifically_provided :
                when_creating_a_type_that_has_constructor_parameters_that_cant_be_faked
            {
                Establish c = () =>
                {
                    the_item = new SomeOtherType(3);
                    constructor_parameters.Add(typeof(SomeOtherType), the_item);
                };

                Because b = () =>
                    result = sut.create();

                It should_be_able_to_be_created_without_issues = () =>
                    result.other.ShouldEqual(the_item);

                static SomeOtherType the_item;
            }
        }

        [Subject(typeof(DefaultSUTFactory<>))]
        public class when_creating_an_item_and_no_constructor_arguments_have_been_provided : concern
        {
            Because b = () =>
                result = sut.create();

            static ItemToCreate created_item;
            static ItemToCreate result;

            It should_return_the_item_created_by_the_greediest_constructor = () =>
            {
                result.connection.ShouldEqual(connection);
                result.command.ShouldEqual(command);
            };
        }

        [Subject(typeof(DefaultSUTFactory<>))]
        public class when_provided_a_specific_constructor_parameter_for_the_sut : concern
        {
            public class and_the_parameter_has_not_yet_been_provided :
                when_provided_a_specific_constructor_parameter_for_the_sut
            {
                Because b = () =>
                    sut.on(3);

                It should_store_the_parameter_for_use_in_fallback_creation = () =>
                    constructor_parameters[typeof(int)].ShouldEqual(3);
            }

            public class and_a_parameter_for_that_type_has_already_been_provided :
                when_provided_a_specific_constructor_parameter_for_the_sut
            {
                Establish c = () =>
                    constructor_parameters[typeof(int)] = 3;

                Because b = () =>
                    sut.on(4);

                It should_overwrite_the_previous_value = () =>
                    constructor_parameters[typeof(int)].ShouldEqual(4);
            }
        }

        [Subject(typeof(DefaultSUTFactory<>))]
        public class when_provided_a_specific_factory_method : concern
        {
            Establish c = () =>
                created_item = new ItemToCreate(null, null);

            Because b = () =>
                sut.create_using(() => new ItemToCreate(null, null));

            It should_use_the_factory_method_for_creation = () =>
                sut.create().ShouldNotEqual(created_item);

            protected static ItemToCreate created_item;
        }

        [Subject(typeof(DefaultSUTFactory<>))]
        public class when_providing_a_constructor_argument_for_the_sut_and_an_explicit_factory_has_been_provided :
            concern
        {
            Establish c = () =>
                sut.create_using(provided_factory);

            Because b = () =>
                sut.on(3);

            It should_not_change_the_value_of_the_sut_factory =
                () => { sut.downcast_to<DefaultSUTFactory<ItemToCreate>>().actual_factory.ShouldEqual(provided_factory); };

            protected static CreateSUT<ItemToCreate> provided_factory;
        }

        public class integration : Observes
        {
            protected static IManageFakes manage_fakes;
            protected static IResolveADependencyForTheSUT dependency_resolver;

            Establish c = () =>
            {
                manage_fakes = new FakesAdapter(new SpecificationController<ItemWithNonFakeableCtorParameters2>(
                                                    new RhinoFakeEngine()));
                dependency_resolver = new SUTDependencyResolver(manage_fakes);
            };

            protected static IDictionary<Type, object> constructor_parameters = new Dictionary<Type, object>();

            protected static DefaultSUTFactory<ForItem> create_sut<ForItem>()
            {
                return new DefaultSUTFactory<ForItem>(constructor_parameters, dependency_resolver, manage_fakes);
            }

            [Subject(typeof(DefaultSUTFactory<>))]
            public class
                when_constructing_a_type_that_has_non_fakeable_dependencies_that_has_struct_dependencies_that_have_not_been_provided :
                    integration
            {
                Establish c = () => { sut = create_sut<ItemWithNonFakeableCtorParameters2>(); };

                Because b = () =>
                    result = sut.create();


                It should_be_able_to_create_it_without_issues = () =>
                    result.ShouldNotBeNull();

                static DefaultSUTFactory<ItemWithNonFakeableCtorParameters2> sut;
                static ItemWithNonFakeableCtorParameters2 result;
            }
        }

        public class ItemWithNonFakeableCtorParameters2
        {
            public IDbConnection connection2 { get; set; }
            public IDbCommand command;
            public IDbConnection connection;
            public int number;
            public int number2;
            public DateTime date;
            public Func<int, bool> condition;

            public ItemWithNonFakeableCtorParameters2(IDbConnection connection, IDbCommand command,
                                                      IDbConnection connection2, int number,
                                                      int number2, DateTime date, Func<int, bool> condition)
            {
                this.connection2 = connection2;
                this.connection = connection;
                this.command = command;
                this.number = number;
                this.number2 = number2;
                this.date = date;
                this.condition = condition;
            }
        }

        public class ItemToCreate
        {
            public IDbCommand command;
            public IDbConnection connection;

            public ItemToCreate(IDbConnection connection, IDbCommand command)
            {
                this.connection = connection;
                this.command = command;
            }
        }

        public class ItemWithNonFakeableCtorParameters
        {
            public IDbCommand command;
            public IDbConnection connection;
            public SomeOtherType other;

            public ItemWithNonFakeableCtorParameters(IDbConnection connection, IDbCommand command, SomeOtherType other)
            {
                this.connection = connection;
                this.command = command;
                this.other = other;
            }
        }
    }

    public class SomeOtherType
    {
        int number;

        public SomeOtherType(int number)
        {
            this.number = number;
        }
    }
}